class Perlnavigator < Formula
  desc "Perl language server"
  homepage "https://github.com/bscan/PerlNavigator"
  url "https://registry.npmjs.org/perlnavigator-server/-/perlnavigator-server-0.8.20.tgz"
  sha256 "2e50dead7e169f865902ef6e447761705f9ef07b89df0bc5b754ff3d484112db"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "716018db131fe389c0348095bece6e52e21163af77deab40b103c860bc076e23"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    require "open3"

    request = {
      jsonrpc: "2.0",
      id:      1,
      method:  "initialize",
      params:  { rootUri: nil, capabilities: {} },
    }.to_json

    Open3.popen3(bin/"perlnavigator", "--stdio") do |stdin, stdout|
      stdin.write "Content-Length: #{request.bytesize}\r\n\r\n#{request}"
      assert_match(/^Content-Length: \d+/i, stdout.readline)
    end
  end
end
