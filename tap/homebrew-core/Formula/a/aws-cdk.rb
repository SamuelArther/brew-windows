class AwsCdk < Formula
  desc "AWS Cloud Development Kit - framework for defining AWS infra as code"
  homepage "https://github.com/aws/aws-cdk"
  url "https://registry.npmjs.org/aws-cdk/-/aws-cdk-2.1146.1.tgz"
  sha256 "9bbd33b1fc96486a55f054f65643aea9d9c11fcbfdb3f5dac32dbcb2a3f02c0e"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bf0ad32d16f920f3e7b984528d954403824bc2c30eca0d717d4cd870bdb4347d"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    # `cdk init` cannot be run in a non-empty directory
    mkdir "testapp" do
      shell_output("#{bin}/cdk init app --language=javascript")
      list = shell_output("#{bin}/cdk list")
      cdkversion = shell_output("#{bin}/cdk --version")
      assert_match "TestappStack", list
      assert_match version.to_s, cdkversion
    end
  end
end
