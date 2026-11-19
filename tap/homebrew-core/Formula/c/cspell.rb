class Cspell < Formula
  desc "Spell checker for code"
  homepage "https://cspell.org"
  url "https://registry.npmjs.org/cspell/-/cspell-10.5.2.tgz"
  sha256 "ff12c624f1b2b1ad2780e81279d333ac07ab8f4b3c24b44f1fd095020cdcae01"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fbcca00c4460bde83e3e5926d84d94847f443049a77b5e56ea91f2d565233f86"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    # Skip linking cspell-esm binary, which is identical to cspell.
    bin.install_symlink libexec/"bin/cspell"

    # Replace code comment to build :all bottle
    node_modules = libexec/"lib/node_modules/cspell/node_modules"
    inreplace node_modules/"global-directory/index.js", "/opt/homebrew", ""
  end

  test do
    (testpath/"test.rb").write("misspell_worrd = 1")
    output = shell_output("#{bin}/cspell test.rb", 1)
    assert_match "test.rb:1:10 - Unknown word (worrd)", output
  end
end
