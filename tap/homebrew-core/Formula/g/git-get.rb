class GitGet < Formula
  desc "Better way to clone, organize and manage multiple git repositories"
  homepage "https://github.com/grdl/git-get"
  url "https://github.com/grdl/git-get/releases/download/v0.6.1/git-get_0.6.1_windows_amd64vv1.zip"
  sha256 "26a88754984a6df10bf64c780445181d71ba8c43cccbaa7eac3760f5abe56bb9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/grdl/git-get/archive/refs/tags/v0.6.1.tar.gz"
  end

  def install
    bin.install "git-get.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-get --version 2>&1")
  end
end
