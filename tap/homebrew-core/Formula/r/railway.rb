class Railway < Formula
  desc "Develop and deploy code with zero configuration"
  homepage "https://railway.com/"
  url "https://github.com/railwayapp/cli/releases/download/v5.68.0/railway-v5.68.0-x86_64-pc-windows-gnu.tar.gz"
  sha256 "b883ca5eee8758aaeca4b3f4a203ed6a3040ac4ac2c78bc354418fbc17b72849"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/railwayapp/cli/archive/refs/tags/v5.68.0.tar.gz"
  end

  def install
    bin.install "railway.exe"
  end

  test do
    output = shell_output("#{bin}/railway init 2>&1", 1).chomp
    assert_match "Unauthorized. Please login with `railway login`", output

    assert_equal "railway #{version}", shell_output("#{bin}/railway --version").strip
  end
end
