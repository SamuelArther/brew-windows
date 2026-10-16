class GithubMarkdownToc < Formula
  desc "Easy TOC creation for GitHub README.md (in go)"
  homepage "https://github.com/ekalinin/github-markdown-toc.go"
  url "https://github.com/ekalinin/github-markdown-toc.go/releases/download/v2.0.1/gh-md-toc_2.0.1_windows_amd64.tar.gz"
  sha256 "047891943be98750dd94d3392f4ae82412fe2e815221d9d16878a0118b49d150"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ekalinin/github-markdown-toc.go/archive/refs/tags/v2.0.1.tar.gz"
  end

  def install
    bin.install "gh-md-toc.exe"
  end

  test do
    (testpath/"README.md").write <<~MARKDOWN
      # Header
    MARKDOWN
    assert_path_exists testpath/"README.md"
    assert_match version.to_s, shell_output("#{bin}/gh-md-toc --version 2>&1")
    assert_match "* [Header](#header)", shell_output("#{bin}/gh-md-toc ./README.md")
  end
end
