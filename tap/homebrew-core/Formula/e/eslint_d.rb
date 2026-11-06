class EslintD < Formula
  desc "Speed up eslint to accelerate your development workflow"
  homepage "https://github.com/mantoni/eslint_d.js"
  url "https://registry.npmjs.org/eslint_d/-/eslint_d-15.1.0.tgz"
  sha256 "f5ade803542ae99899eaf49c68a2678266f32b44cb4a6a828d0029fefadc1ca7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "610405e48cb7372c528da5aef8975b21d2c67a99cc69cf28bb59f515f5976e19"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  service do
    run [opt_bin/"eslint_d", "start"]
    keep_alive true
    working_dir var/"eslint_d"
    log_path var/"log/eslint_d.log"
    error_log_path var/"log/eslint_d.err.log"
  end

  test do
    output = shell_output("#{bin}/eslint_d status")
    assert_match "eslint_d: Not running", output

    assert_match version.to_s, shell_output("#{bin}/eslint_d --version")
  end
end
