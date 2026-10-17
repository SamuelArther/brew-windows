class Babel < Formula
  desc "Compiler for writing next generation JavaScript"
  homepage "https://babeljs.io/"
  url "https://registry.npmjs.org/@babel/cli/-/cli-8.0.6.tgz"
  sha256 "dfbbfcb4c7ec02ea76a7681b18990977e3cff412cb8420e1346442b05129c816"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d1472e2863134e05711603798458c384ad2e228aa50055d91c0b7c9764870525"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"script.js").write <<~JS
      [1,2,3].map(n => n + 1);
    JS

    system bin/"babel", "script.js", "--out-file", "script-compiled.js"
    assert_path_exists testpath/"script-compiled.js", "script-compiled.js was not generated"
  end
end
