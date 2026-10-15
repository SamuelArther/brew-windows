class Mado < Formula
  desc "Fast Markdown linter written in Rust"
  homepage "https://github.com/akiomik/mado"
  url "https://github.com/akiomik/mado/releases/download/v0.3.2/mado-Windows-msvc-x86_64.zip"
  sha256 "4d04095cccaad58319ad1094575797dc92d7488413f18099768865e4c47fb8e7"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/akiomik/mado/archive/refs/tags/v0.3.2.tar.gz"
  end

  def install
    bin.install "mado.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mado --version")

    (testpath/"bad.md").write <<~MARKDOWN
      # Heading 1
      body without blank line
    MARKDOWN
    refute_empty shell_output("#{bin}/mado check #{testpath}/bad.md 2>&1", 1)

    (testpath/"good.md").write <<~MARKDOWN
      # Heading 1

      body with blank line
    MARKDOWN
    assert_match "All checks passed!", shell_output("#{bin}/mado check #{testpath}/good.md")
  end
end
