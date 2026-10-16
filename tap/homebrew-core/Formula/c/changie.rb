class Changie < Formula
  desc "Automated changelog tool for preparing releases"
  homepage "https://changie.dev/"
  url "https://github.com/miniscruff/changie/releases/download/v1.26.0/changie_1.26.0_windows_amd64.zip"
  sha256 "86b1e2851d78be2b52b04c8129442d4fcc8b29edd839bcc893fe66486d72ae72"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/miniscruff/changie/archive/refs/tags/v1.26.0.tar.gz"
  end

  def install
    bin.install "changie.exe"
  end

  test do
    system bin/"changie", "init"
    assert_match "All notable changes to this project", (testpath/"CHANGELOG.md").read

    assert_match version.to_s, shell_output("#{bin}/changie --version")
  end
end
