class Step < Formula
  desc "Crypto and x509 Swiss-Army-Knife"
  homepage "https://smallstep.com"
  url "https://github.com/smallstep/cli/releases/download/v0.31.0/step_windows_0.31.0_amd64.zip"
  sha256 "3b8dc1245fb1dffe3b57bf10cac9b3da9808ed42f7274f869085e1086214d0f4"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "bin/step.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/step version")
  end
end
