import unittest

from tools.docs.generate_docs_index import enrich_relationships


def doc(path, role, related=()):
    return {"path": path, "role": role, "related": list(related)}


class EnrichRelationshipsTests(unittest.TestCase):
    def test_one_way_related_produces_backlink_and_shared_count(self):
        docs = enrich_relationships(
            [doc("a.md", "程序", ["b.md"]), doc("b.md", "美术"), doc("c.md", "程序", ["a.md"])]
        )
        by_path = {item["path"]: item for item in docs}
        self.assertEqual(by_path["a.md"]["referenced_by"], ["c.md"])
        self.assertEqual(by_path["b.md"]["referenced_by"], ["a.md"])
        self.assertEqual(by_path["a.md"]["relation_count"], 2)
        self.assertEqual(by_path["b.md"]["relation_count"], 1)
        self.assertEqual(by_path["b.md"]["cross_role_relation_count"], 1)

    def test_bidirectional_pair_counts_once(self):
        docs = enrich_relationships([doc("a.md", "全局", ["b.md"]), doc("b.md", "全局", ["a.md"])])
        self.assertEqual([item["relation_count"] for item in docs], [1, 1])

    def test_missing_and_self_targets_are_ignored(self):
        docs = enrich_relationships([doc("a.md", "全局", ["a.md", "gone.md"])])
        self.assertEqual((docs[0]["relation_count"], docs[0]["referenced_by"]), (0, []))


if __name__ == "__main__":
    unittest.main()
