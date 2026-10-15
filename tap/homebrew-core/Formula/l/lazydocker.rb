class Lazydocker < Formula
  desc "Lazier way to manage everything docker"
  homepage "https://github.com/jesseduffield/lazydocker"
  url "https://github.com/jesseduffield/lazydocker/releases/download/v0.25.2/lazydocker_0.25.2_Windows_x86_64.zip"
  sha256 "facae8869c1bf144468191c563002162273a2c2761f052659170530cae3fa8a9"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "lazydocker.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lazydocker --version")
  end
end
