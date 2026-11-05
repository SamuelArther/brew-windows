class FernApi < Formula
  desc "Stripe-level SDKs and Docs for your API"
  homepage "https://buildwithfern.com/"
  url "https://registry.npmjs.org/fern-api/-/fern-api-5.195.0.tgz"
  sha256 "0aabbd4e600ca5f8e46b5ceb098cfe9d1a0cea72809013a94b525863aecc2098"
  license "Apache-2.0"

  livecheck do
    throttle days: 1
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "7d498b81c4cb363194914f012fd17039957cd7024a574495c808f140a63e6fd0"
  end

  depends_on "node"

  def install
    # Supress self update notifications
    inreplace "cli.cjs", "await this.nudgeUpgradeIfAvailable()", "await 0"
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    system bin/"fern", "init", "--docs", "--org", "brewtest"
    assert_path_exists testpath/"fern/docs.yml"
    assert_match '"organization": "brewtest"', (testpath/"fern/fern.config.json").read

    system bin/"fern", "--version"
  end
end
