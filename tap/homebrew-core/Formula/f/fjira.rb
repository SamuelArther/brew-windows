class Fjira < Formula
  desc "Fuzzy-find cli jira interface"
  homepage "https://github.com/mk-5/fjira"
  url "https://github.com/mk-5/fjira/releases/download/1.5.6/fjira_Windows_x86_64.zip"
  sha256 "34af7a219cbbaa9eb5a48fffa2a7df538439a5ef6c8b7f6749b88cd029d8948e"
  license "AGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mk-5/fjira/archive/refs/tags/1.5.6.tar.gz"
  end

  def install
    bin.install "fjira.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fjira version 2>&1")
  end
end
