class Jabba < Formula
  desc "Cross-platform Java Version Manager"
  homepage "https://github.com/Jabba-Team/jabba"
  url "https://github.com/Jabba-Team/jabba/releases/download/0.15.0/jabba-0.15.0-windows-amd64.exe"
  sha256 "8918f09b6632f7e3b3d14a74c9d07af86d589230804b38d09ee1412d29337a71"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Jabba-Team/jabba/archive/refs/tags/0.15.0.tar.gz"
  end

  def install
    bin.install "jabba-0.15.0-windows-amd64.exe" => "jabba.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jabba --version 2>&1")
  end
end
