#!/usr/bin/env python3
"""Fetch official Great Courses metadata/artwork into a Jellyfin library folder."""

from __future__ import annotations

import argparse
import html
import os
import re
import sys
import time
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path


COURSES = {
    "Great Courses The Black Death": "the-black-death-the-worlds-most-devastating-plague",
    "Great Courses Books That Matter The History of the Decline and Fall of the Roman Empire": "books-that-matter-the-history-of-the-decline-and-fall-of-the-roman-empire",
    "Great Courses Classical Mythology": "classical-mythology",
    "Great Courses Effective Editing How to Take Your Writing to the Next Level": "effective-editing-how-to-take-your-writing-to-the-next-level",
    "Great Courses How the Medici Shaped the Renaissance": "how-the-medici-shaped-the-renaissance",
    "Great Courses How to Write Best-Selling Fiction": "how-to-write-best-selling-fiction",
    "Great Courses Pioneering Skills for Everyone Modern Homesteading": "pioneering-skills-for-everyone-modern-homesteading",
    "Great Courses The Everyday Guide to Beer": "the-everyday-guide-to-beer",
    "Great Courses The Great Tours Iceland": "the-great-tours-iceland",
    "Great Courses The Great Tours Ireland and Northern Ireland": "the-great-tours-ireland-and-northern-ireland",
    "Great Courses The Ottoman Empire": "the-ottoman-empire",
    "Great Courses Understanding the New Testament": "understanding-the-new-testament",
    "Great Courses World War II The Pacific Theater": "world-war-ii-the-pacific-theater-8756",
}

MEDIA_EXTENSIONS = {".m4a", ".m4b", ".mkv", ".mp3", ".mp4"}
USER_AGENT = "Mozilla/5.0 (compatible; JellyfinGreatCoursesMetadata/0.1)"


def fetch_text(url: str) -> str:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.read().decode("utf-8", "ignore")


def fetch_bytes(url: str) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.read()


def strip_html(value: str) -> str:
    value = re.sub(r"<script.*?</script>|<style.*?</style>", " ", value, flags=re.I | re.S)
    value = re.sub(r"<[^>]+>", "\n", value)
    value = html.unescape(value)
    value = re.sub(r"[ \t\r\f\v]+", " ", value)
    value = re.sub(r"\n\s*\n+", "\n", value)
    return value.strip()


def parse_course(page_html: str, source_url: str) -> dict[str, object]:
    text = strip_html(page_html)
    title_match = re.search(r"<h1[^>]*>(.*?)</h1>", page_html, flags=re.I | re.S)
    title = html.unescape(re.sub(r"<[^>]+>", "", title_match.group(1))).strip() if title_match else ""

    course_match = re.search(r"Course No\.\s*(\d+)", text)
    course_number = course_match.group(1) if course_match else ""

    professor = ""
    about_match = re.search(r"\nAbout\n\s*([^\n]+)", text)
    if about_match:
        professor = about_match.group(1).strip()

    overview = ""
    overview_match = re.search(r"Course No\.\s*\d+\n(?P<overview>.*?)(?:\nAbout\n|\nView Full Details\n|\n#### About\n)", text, flags=re.S)
    if overview_match:
        overview = re.sub(r"\s+", " ", overview_match.group("overview")).strip()

    image_url = ""
    if course_number:
        image_match = re.search(
            rf"https://secureimages\.teach12\.com/tgc/images/m2/wondrium/courses/{course_number}/{course_number}\.jpg",
            page_html,
        )
        if image_match:
            image_url = image_match.group(0)

    lectures = []
    for match in re.finditer(
        r"\n\s*(?P<number>\d{2}):\s*\n\s*(?P<title>[^\n]+)\n(?P<body>.*?)\n\d+\s+min\n",
        text,
        flags=re.S,
    ):
        body = re.sub(r"\s+", " ", match.group("body")).strip()
        lectures.append(
            {
                "number": int(match.group("number")),
                "title": match.group("title").strip(),
                "overview": body,
            }
        )

    return {
        "title": title,
        "course_number": course_number,
        "professor": professor,
        "overview": overview,
        "image_url": image_url,
        "lectures": lectures,
        "source_url": source_url,
    }


def write_xml(path: Path, root: ET.Element) -> None:
    ET.indent(root)
    tree = ET.ElementTree(root)
    tree.write(path, encoding="utf-8", xml_declaration=True)


def add_text(parent: ET.Element, name: str, value: object | None) -> None:
    if value is None or value == "":
        return
    ET.SubElement(parent, name).text = str(value)


def write_tvshow_nfo(course_dir: Path, course: dict[str, object]) -> None:
    root = ET.Element("tvshow")
    add_text(root, "title", course["title"])
    add_text(root, "sorttitle", course["title"])
    add_text(root, "plot", course["overview"])
    add_text(root, "id", course["course_number"])
    add_text(root, "source", course["source_url"])
    add_text(root, "studio", "The Great Courses")
    add_text(root, "genre", "Education")
    if course.get("professor"):
        actor = ET.SubElement(root, "actor")
        add_text(actor, "name", course["professor"])
        add_text(actor, "role", "Instructor")
    poster = ET.SubElement(root, "thumb", {"aspect": "poster"})
    poster.text = "poster.jpg"
    landscape = ET.SubElement(root, "thumb", {"aspect": "landscape"})
    landscape.text = "landscape.jpg"
    write_xml(course_dir / "tvshow.nfo", root)


def episode_number(path: Path) -> int | None:
    match = re.search(r"S\d{2}E(?P<episode>\d{2,3})", path.stem, flags=re.I)
    return int(match.group("episode")) if match else None


def write_episode_nfos(course_dir: Path, course: dict[str, object]) -> int:
    lectures = {lecture["number"]: lecture for lecture in course["lectures"]}  # type: ignore[index]
    written = 0
    for media in sorted(p for p in course_dir.rglob("*") if p.suffix.lower() in MEDIA_EXTENSIONS):
        number = episode_number(media)
        if number is None:
            continue
        lecture = lectures.get(number, {})
        root = ET.Element("episodedetails")
        add_text(root, "title", lecture.get("title") or media.stem)
        add_text(root, "showtitle", course["title"])
        add_text(root, "season", 1)
        add_text(root, "episode", number)
        add_text(root, "plot", lecture.get("overview"))
        write_xml(media.with_suffix(".nfo"), root)
        written += 1
    return written


def write_artwork(course_dir: Path, image_url: str, overwrite: bool) -> bool:
    if not image_url:
        return False
    targets = [course_dir / "folder.jpg", course_dir / "poster.jpg", course_dir / "landscape.jpg"]
    if not overwrite and all(target.exists() for target in targets):
        return False
    data = fetch_bytes(image_url)
    for target in targets:
        if overwrite or not target.exists():
            target.write_bytes(data)
    return True


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("library", type=Path)
    parser.add_argument("--overwrite-images", action="store_true")
    args = parser.parse_args()

    if not args.library.is_dir():
        print(f"Library folder does not exist: {args.library}", file=sys.stderr)
        return 1

    total_nfos = 0
    total_images = 0
    for course_dir in sorted(path for path in args.library.iterdir() if path.is_dir()):
        slug = COURSES.get(course_dir.name)
        if not slug:
            print(f"SKIP no slug mapping: {course_dir.name}")
            continue

        source_url = f"https://shop.thegreatcourses.com/{slug}"
        print(f"FETCH {course_dir.name}")
        course = parse_course(fetch_text(source_url), source_url)
        write_tvshow_nfo(course_dir, course)
        nfos = write_episode_nfos(course_dir, course)
        image_written = write_artwork(course_dir, str(course["image_url"]), args.overwrite_images)
        total_nfos += nfos + 1
        total_images += 1 if image_written else 0
        print(f"  title={course['title']!r} course={course['course_number']} lectures={len(course['lectures'])} nfos={nfos} image={'yes' if image_written else 'no'}")
        time.sleep(0.25)

    print(f"DONE nfos={total_nfos} images={total_images}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
