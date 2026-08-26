"""Executable tests for the authored StackYoga logical-key adapter."""

import unittest

from stackyoga_yogabase import build_storage_object_url, storage_object_for_logical_key


class StorageLogicalKeyTests(unittest.TestCase):
    def test_hierarchical_key_preserves_slashes_and_encodes_segments(self) -> None:
        self.assertEqual(
            build_storage_object_url(
                "https://apps.example.test:8443/",
                "avatars/My photo (1).png",
            ),
            "https://apps.example.test:8443/v1/storage/objects/avatars/My%20photo%20%281%29.png",
        )
        self.assertEqual(
            build_storage_object_url("https://apps.example.test/", "café/猫.json"),
            "https://apps.example.test/v1/storage/objects/caf%C3%A9/%E7%8C%AB.json",
        )

    def test_invalid_keys_fail_closed(self) -> None:
        invalid = [
            "", " ", "/a", "projects/a", "https://example.test/a", "a\\b", "a%b",
            "a\0b", "a//b", ".", "..", "a/./b", "a/../b", "x" * 1025,
        ]
        for value in invalid:
            with self.subTest(value=value), self.assertRaises((TypeError, ValueError)):
                build_storage_object_url("https://apps.example.test/", value)

    def test_adapter_uses_generated_with_url_override(self) -> None:
        calls: list[tuple[str, str]] = []
        expected = object()

        class Item:
            def with_url(self, raw_url: str):
                calls.append(("with_url", raw_url))
                return expected

        class Objects:
            def by_key(self, key: str):
                calls.append(("by_key", key))
                return Item()

        actual = storage_object_for_logical_key(
            Objects(),
            "https://apps.example.test/",
            "avatars/user 1/photo.png",
        )
        self.assertIs(actual, expected)
        self.assertEqual(
            calls,
            [
                ("by_key", "_stackyoga_raw_url_"),
                ("with_url", "https://apps.example.test/v1/storage/objects/avatars/user%201/photo.png"),
            ],
        )


if __name__ == "__main__":
    unittest.main()
