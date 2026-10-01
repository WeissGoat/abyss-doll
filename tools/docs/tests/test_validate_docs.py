import unittest

from tools.docs.validate_docs import (
    second_level_headings,
    validate_body_char_limit,
    validate_body_line_limit,
    validate_line_char_limit,
    validate_no_dated_logs,
    validate_required_headings,
)


class ValidateDocsStructureTests(unittest.TestCase):
    def test_second_level_headings_extracts_exact_titles(self):
        body = "# 标题\n\n## 当前关注\n内容\n## 问题 / 阻塞\n无\n"
        self.assertEqual(second_level_headings(body), ["当前关注", "问题 / 阻塞"])

    def test_required_headings_accepts_valid_snapshot(self):
        body = "\n".join(
            [
                "# 状态",
                "## 最后更新",
                "2026-07-18",
                "## 当前关注",
                "内容",
                "## 最近完成",
                "内容",
                "## 下一步建议",
                "内容",
                "## 问题 / 阻塞",
                "无",
                "## 关键证据入口",
                "- evidence",
            ]
        )
        required = {
            "最后更新",
            "当前关注",
            "最近完成",
            "下一步建议",
            "问题 / 阻塞",
            "关键证据入口",
        }
        self.assertEqual(validate_required_headings("status.md", body, required), [])

    def test_required_headings_reports_missing_heading(self):
        errors = validate_required_headings(
            "view.md",
            "# View\n## 必读\n## 按任务读取\n",
            {"必读", "按任务读取", "验收 / 恢复时"},
        )
        self.assertEqual(errors, ["view.md missing required heading: 验收 / 恢复时"])

    def test_body_line_limit_reports_oversized_body(self):
        body = "\n".join(f"line {index}" for index in range(81))
        self.assertEqual(
            validate_body_line_limit("status.md", body, 80),
            ["status.md body exceeds 80 lines: 81"],
        )

    def test_body_char_limit_counts_characters_not_bytes(self):
        body = "# 状态\n" + "零" * 20
        self.assertEqual(validate_body_char_limit("status.md", body, 25), [])
        self.assertEqual(
            validate_body_char_limit("status.md", body + "号", 25),
            ["status.md body exceeds 25 characters: 26"],
        )

    def test_body_char_limit_ignores_crlf(self):
        self.assertEqual(validate_body_char_limit("status.md", "a\r\nb\r\n", 4), [])

    def test_line_char_limit_reports_each_long_line(self):
        body = "# 状态\n- " + "长" * 10 + "\n- 短\n"
        self.assertEqual(
            validate_line_char_limit("status.md", body, 8),
            ["status.md body line 2 exceeds 8 characters: 12"],
        )

    def test_dated_top_level_heading_is_rejected(self):
        body = "# 状态\n## 当前关注\n内容\n# 2026-07-12 验收入口\n"
        self.assertEqual(
            validate_no_dated_logs("status.md", body),
            ["status.md contains dated log heading: # 2026-07-12 验收入口"],
        )

    def test_dated_log_heading_is_rejected(self):
        body = "# 状态\n## 2026-07-18 某次完成\n- 记录\n"
        self.assertEqual(
            validate_no_dated_logs("status.md", body),
            ["status.md contains dated log heading: ## 2026-07-18 某次完成"],
        )

    def test_dated_log_bullet_is_rejected(self):
        body = "# 项目状态\n## 最近完成\n- 2026-07-18 某次完成\n"
        self.assertEqual(
            validate_no_dated_logs("PROJECT_STATUS.md", body),
            ["PROJECT_STATUS.md contains dated log entry: - 2026-07-18 某次完成"],
        )

    def test_valid_role_view_structure_passes(self):
        body = "# View\n## 必读\n内容\n## 按任务读取\n内容\n## 验收 / 恢复时读取\n内容\n"
        errors = validate_required_headings("view.md", body, {"必读", "按任务读取"})
        headings = second_level_headings(body)
        self.assertEqual(errors, [])
        self.assertTrue(any(heading.startswith("验收 / 恢复时") for heading in headings))


if __name__ == "__main__":
    unittest.main()
