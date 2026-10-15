class Grype < Formula
  desc "Vulnerability scanner for container images and filesystems"
  homepage "https://github.com/anchore/grype"
  url "https://github.com/anchore/grype/releases/download/v0.120.1/grype_0.120.1_windows_amd64.zip"
  sha256 "32e3c811f31822d17592908bafdc6288aaaca3d52583c479167a8dc8399ed65d"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "grype.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/grype version")
  end
end
