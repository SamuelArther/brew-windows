class GitInteractiveRebaseTool < Formula
  desc "Native sequence editor for Git interactive rebase"
  homepage "https://gitrebasetool.mitmaro.ca/"
  url "https://github.com/MitMaro/git-interactive-rebase-tool/releases/download/2.4.1/git-interactive-rebase-tool-2.4.1-windows-_x86_64.exe"
  sha256 "c1ed22c2fe4d6f5d642714ad0c3bdad877805680460f5ada87ecd006d4d73440"
  license "GPL-3.0-or-later"
  revision 4

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MitMaro/git-interactive-rebase-tool/archive/refs/tags/2.4.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "git-interactive-rebase-tool-2.4.1-windows-_x86_64.exe" => "interactive-rebase-tool.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/interactive-rebase-tool --version 2>&1")
  end
end
