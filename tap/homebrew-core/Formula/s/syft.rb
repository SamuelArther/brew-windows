class Syft < Formula
  desc "CLI for generating a Software Bill of Materials from container images"
  homepage "https://github.com/anchore/syft"
  url "https://github.com/anchore/syft/releases/download/v1.54.1/syft_1.54.1_windows_amd64.zip"
  sha256 "8b56e8285e295e0bbed26eeea9b16ed51c493be97ccdf42dae6326c84fe8e19f"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "syft.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/syft --version")
  end
end
