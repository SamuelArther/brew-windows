class Aqua < Formula
  desc "Declarative CLI Version manager"
  homepage "https://aquaproj.github.io/"
  url "https://github.com/aquaproj/aqua/releases/download/v2.64.0/aqua_windows_amd64.zip"
  sha256 "7f8694f6181a01360633c27f8c9c4140d97f4b9d6fb244bad6b5bdfe8b2b7916"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/aquaproj/aqua/archive/refs/tags/v2.64.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "aqua.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aqua --version")

    system bin/"aqua", "init"
    assert_match "depName=aquaproj/aqua-registry", (testpath/"aqua.yaml").read
  end
end
