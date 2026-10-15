class Multimarkdown < Formula
  desc "Turn marked-up plain text into well-formatted documents"
  homepage "https://fletcher.github.io/MultiMarkdown-6/"
  url "https://github.com/fletcher/MultiMarkdown-6/releases/download/6.7.0/MultiMarkdown-Windows-6.7.0.zip"
  sha256 "12163d418a7475032df435fa5ebb4e48d9c99f78e0aa130093b0344ca7c0cf8b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fletcher/MultiMarkdown-6/archive/refs/tags/6.8.0.tar.gz"
  end

  conflicts_with "discount", because: "both install `markdown` binaries"
  conflicts_with "mtools", because: "both install `mmd` binaries"

  def install
    install_tree
    windows_cmd "libexec/bin/markdown.bat", "markdown"
    windows_cmd "libexec/bin/mmd.bat", "mmd"
    windows_cmd "libexec/bin/mmd2epub.bat", "mmd2epub"
    windows_cmd "libexec/bin/mmd2fodt.bat", "mmd2fodt"
    windows_cmd "libexec/bin/mmd2odt.bat", "mmd2odt"
    windows_cmd "libexec/bin/mmd2opml.bat", "mmd2opml"
    windows_cmd "libexec/bin/mmd2tex.bat", "mmd2tex"
    windows_exe "libexec/bin/multimarkdown.exe", "multimarkdown"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/markdown --version 2>&1")
  end
end
