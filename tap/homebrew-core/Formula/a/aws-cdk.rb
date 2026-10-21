class AwsCdk < Formula
  desc "AWS Cloud Development Kit - framework for defining AWS infra as code"
  homepage "https://github.com/aws/aws-cdk"
  url "https://registry.npmjs.org/aws-cdk/-/aws-cdk-2.1146.0.tgz"
  sha256 "2eba07de2aee95735f7f329ff40f7bc3507857b5f5db1e2441e2b438f8d13000"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9775f81608297aa41a942d3ac4cb1b7ade690180bc8630a5a136ffb6500b3adb"
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
