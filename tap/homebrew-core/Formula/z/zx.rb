class Zx < Formula
  desc "Tool for writing better scripts"
  homepage "https://google.github.io/zx/"
  url "https://registry.npmjs.org/zx/-/zx-8.8.5.tgz"
  sha256 "20df3c1b6160372f02bd26176898c9b791690af4599573c99a04b2624a6aaab9"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "594c068af3844cbb7460c41fdcb9e20a1513c599342fc25554f1ee212d798a14"
  end

  depends_on "node"

  deny_network_access!

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test.mjs").write <<~JAVASCRIPT
      #!/usr/bin/env zx

      let name = YAML.parse('foo: bar').foo
      console.log(`name is ${name}`)
      await $`touch ${name}`
    JAVASCRIPT

    output = shell_output("#{bin}/zx #{testpath}/test.mjs")
    assert_match "name is bar", output
    assert_path_exists testpath/"bar"

    assert_match version.to_s, shell_output("#{bin}/zx --version")
  end
end
