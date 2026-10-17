class PatchPackage < Formula
  desc "Fix broken node modules instantly"
  homepage "https://github.com/ds300/patch-package"
  url "https://registry.npmjs.org/patch-package/-/patch-package-8.0.1.tgz"
  sha256 "fb36b47e1f22a3d612311bc9a0464b0213e0971958cfabb8f26a9537b1d7c4f7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1c55f3eb67611f7afabd666cf2428a03ac52100709696a5eea78b4f63ff6608f"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    output = shell_output("#{bin}/patch-package 2>&1", 1)
    assert_match "no package.json found for this project", output

    (testpath/"package.json").write <<~JSON
      {
        "name": "brewtest",
        "version": "1.0.0"
      }
    JSON

    expected = <<~EOS
      patch-package #{version}
      Applying patches...
      No patch files found
    EOS
    assert_equal expected, shell_output("#{bin}/patch-package 2>&1")
  end
end
