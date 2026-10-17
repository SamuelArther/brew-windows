class Shush < Formula
  desc "Encrypt and decrypt secrets using the AWS Key Management Service"
  homepage "https://github.com/realestate-com-au/shush"
  url "https://github.com/realestate-com-au/shush/archive/refs/tags/v1.5.5.tar.gz"
  sha256 "b759401d94b2ebcc4a5561e28e1c533f3bd19aaa75eb0a48efc53c71f864e11b"
  license "MIT"
  head "https://github.com/realestate-com-au/shush.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "de9d31ab947007e9f1cf876820ae2d82abe46ed90cff87cdbd8c153848320c2f"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    output = shell_output("#{bin}/shush encrypt brewtest 2>&1", 64)
    assert_match "ERROR: please specify region (--region or $AWS_DEFAULT_REGION)", output

    assert_match version.to_s, shell_output("#{bin}/shush --version")
  end
end
