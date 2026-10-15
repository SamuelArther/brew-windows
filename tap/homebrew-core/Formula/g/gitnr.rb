class Gitnr < Formula
  desc "Create `.gitignore` using templates from TopTal, GitHub or your own collection"
  homepage "https://github.com/reemus-dev/gitnr"
  url "https://github.com/reemus-dev/gitnr/releases/download/v0.3.1/gitnr-win-amd64.exe"
  sha256 "ff8be02c3fe01984ac93a057d6dc97a0e468ea3c75831f79e6f1a3a4abaaae7d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/reemus-dev/gitnr/archive/refs/tags/v0.3.1.tar.gz"
  end

  def install
    bin.install "gitnr-win-amd64.exe" => "gitnr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitnr --version 2>&1")
  end
end
