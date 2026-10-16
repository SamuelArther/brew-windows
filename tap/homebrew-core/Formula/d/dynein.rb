class Dynein < Formula
  desc "DynamoDB CLI"
  homepage "https://github.com/awslabs/dynein"
  url "https://github.com/awslabs/dynein/releases/download/v0.3.0/dynein-windows.zip"
  sha256 "413e3b7dbc222186cbfaed6493be759af5c379ecee32e6e64415124e57a0ba1e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/awslabs/dynein/archive/refs/tags/v0.3.0.tar.gz"
  end

  def install
    bin.install "dy.exe"
  end

  test do
    assert_match "To find all tables in all regions", shell_output("#{bin}/dy desc 2>&1", 1)
    assert_match version.to_s, shell_output("#{bin}/dy --version")
  end
end
