class Mapscii < Formula
  desc "Whole World In Your Console"
  homepage "https://github.com/rastapasta/mapscii"
  url "https://registry.npmjs.org/mapscii/-/mapscii-0.3.1.tgz"
  sha256 "21f18b687975f1decef998664ef6d292cf2379d9e4308a5b4cbb66af240341b3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "01ea0297078352c3ac549aace1d3632156f3bec69083922179b230ae74b8bbb0"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    output_log = testpath/"output.log"
    with_test_process(bin/"mapscii", [:out, :err] => output_log.to_s) do
      sleep 5
      assert_match "Failed to start MapSCII", output_log.read
    end
  end
end
