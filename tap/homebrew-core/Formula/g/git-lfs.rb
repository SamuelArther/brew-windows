class GitLfs < Formula
  desc "Git extension for versioning large files"
  homepage "https://git-lfs.com/"
  version "3.8.0"
  url "https://github.com/git-lfs/git-lfs/releases/download/v3.8.0/git-lfs-windows-amd64-v3.8.0.zip"
  sha256 "b62e7b8ceddee635f691233d77de8eaa4b213e9209e0173811d8cfa77f7882c1"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "git-lfs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-lfs version")
  end
end
