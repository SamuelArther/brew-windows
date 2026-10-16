class DsdaDoom < Formula
  desc "Fork of prboom+ with a focus on speedrunning"
  homepage "https://github.com/kraflab/dsda-doom"
  url "https://github.com/dsda-org/dsda-doom/releases/download/v0.30.1/dsda-doom-0.30.1-win-x64.zip"
  sha256 "a155552be0056de84a50ee9fbeb5111d4276cdcdbf5aced2d920695f03fbd2d2"
  license "GPL-2.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kraflab/dsda-doom/archive/refs/tags/v0.30.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/dsda-doom.exe", "dsda-doom"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dsda-doom -V 2>&1")
  end
end
