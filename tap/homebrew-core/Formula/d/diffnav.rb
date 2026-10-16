class Diffnav < Formula
  desc "Git diff pager based on delta but with a file tree"
  homepage "https://github.com/dlvhdr/diffnav"
  url "https://github.com/dlvhdr/diffnav/releases/download/v0.13.0/diffnav_Windows_x86_64.zip"
  sha256 "2d9fd97556008580f881300a44976d79e90af7ae998c752d7fccbe1dc96afb56"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dlvhdr/diffnav/archive/refs/tags/v0.13.0.tar.gz"
  end

  def install
    bin.install "diffnav.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/diffnav --version 2>&1")
  end
end
