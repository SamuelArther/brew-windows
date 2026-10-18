class Eslint < Formula
  desc "AST-based pattern checker for JavaScript"
  homepage "https://eslint.org"
  url "https://registry.npmjs.org/eslint/-/eslint-10.13.0.tgz"
  sha256 "05da899bc1602385a199570db5ad7cff488010fb103e659994065eed38f479f3"
  license "MIT"
  head "https://github.com/eslint/eslint.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "256072c5dfa17123ac11a3d5c8354d9e4be8e74d83f3c893cfbce6b490975596"
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
