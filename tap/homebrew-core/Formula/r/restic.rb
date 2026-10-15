class Restic < Formula
  desc "Fast, efficient and secure backup program"
  homepage "https://restic.net/"
  version "0.19.1"
  url "https://github.com/restic/restic/releases/download/v0.19.1/restic_0.19.1_windows_amd64.zip"
  sha256 "da948ad707ed690426473aaba2046cd61f8f90f6f0e7dab6be0d5796531de67d"
  license "BSD-2-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "restic_0.19.1_windows_amd64.exe" => "restic.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/restic version")
  end
end
