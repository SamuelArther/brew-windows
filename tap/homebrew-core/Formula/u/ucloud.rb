class Ucloud < Formula
  desc "Official tool for managing UCloud services"
  homepage "https://www.ucloud.cn"
  url "https://github.com/ucloud/ucloud-cli/releases/download/v0.3.10/ucloud-windows_amd64.zip"
  sha256 "b4dc279c98ee9b6625df4efe3d9f3af0c643fda471f05d6d94765f16d7aff127"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ucloud/ucloud-cli/archive/refs/tags/v0.3.10.tar.gz"
  end

  def install
    bin.install "ucloud.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ucloud --version 2>&1")
  end
end
