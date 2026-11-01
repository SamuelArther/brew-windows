class Eslint < Formula
  desc "AST-based pattern checker for JavaScript"
  homepage "https://eslint.org"
  url "https://registry.npmjs.org/eslint/-/eslint-10.14.0.tgz"
  sha256 "e15ffa02262a7d359f9385e2d468f289dd309715261b2e3aa276ccd26ed14720"
  license "MIT"
  head "https://github.com/eslint/eslint.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "de0cb2c6b4d8fe651918e550691206545193a97a8ef4bec5b9a0f62f469fb658"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    # https://eslint.org/docs/latest/use/configure/configuration-files#configuration-file
    (testpath/"eslint.config.js").write("{}") # minimal config
    (testpath/"syntax-error.js").write("{}}")

    # https://eslint.org/docs/user-guide/command-line-interface#exit-codes
    output = shell_output("#{bin}/eslint syntax-error.js", 1)
    assert_match "Unexpected token }", output
  end
end
