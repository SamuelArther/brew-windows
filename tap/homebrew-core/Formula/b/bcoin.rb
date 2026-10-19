class Bcoin < Formula
  desc "Javascript bitcoin library for node.js and browsers"
  # Original homepage https://bcoin.io is down (GitHub Pages TLS cert not provisioned)
  homepage "https://github.com/bcoin-org/bcoin"
  url "https://github.com/bcoin-org/bcoin/archive/refs/tags/v2.2.0.tar.gz"
  sha256 "fa1a78a73bef5837b7ff10d18ffdb47c0e42ad068512987037a01e8fad980432"
  license "MIT"
  head "https://github.com/bcoin-org/bcoin.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a1d50b68b49a6edd165ab085a21c9d386c385012b3453c1aaecc7a59fe6b8e1f"
  end

  depends_on "node"

  uses_from_macos "python" => :build

  def node
    deps.reject(&:build?)
        .map(&:to_formula)
        .find { |f| f.name.match?(/^node(@\d+(\.\d+)*)?$/) }
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    node_modules = libexec/"lib/node_modules/bcoin/node_modules"
    cd node_modules/"bcrypto" do
      system "npm", "run", "install"
    end
    cd node_modules/"bdb" do
      system "npm", "run", "install"
    end
  end

  test do
    (testpath/"script.js").write <<~JS
      const assert = require('assert');
      const bcoin = require('#{libexec}/lib/node_modules/bcoin');
      assert(bcoin);

      const node = new bcoin.FullNode({
        prefix: '#{testpath}/.bcoin',
        memory: false
      });
      (async () => {
        await node.ensure();
      })();
    JS
    system "#{node.opt_bin}/node", testpath/"script.js"
    assert File.directory?("#{testpath}/.bcoin")
  end
end
