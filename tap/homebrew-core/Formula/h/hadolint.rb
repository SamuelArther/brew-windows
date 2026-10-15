class Hadolint < Formula
  desc "Smarter Dockerfile linter to validate best practices"
  homepage "https://github.com/hadolint/hadolint"
  url "https://github.com/hadolint/hadolint/releases/download/v2.15.1/hadolint-windows-x86_64.exe"
  sha256 "01d927294962b5387f9ead4f18679158452be4f17c765ad0bdffe5264b9c7b0a"
  license "GPL-3.0-only"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "hadolint-windows-x86_64.exe" => "hadolint.exe"
  end

  test do
    df = testpath/"Dockerfile"
    df.write <<~DOCKERFILE
      FROM debian
    DOCKERFILE
    assert_match "DL3006", shell_output("#{bin}/hadolint #{df}", 1)
  end
end
