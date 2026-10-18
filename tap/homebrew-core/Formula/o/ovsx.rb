class Ovsx < Formula
  desc "Command-line interface for Eclipse Open VSX"
  homepage "https://www.npmjs.com/package/ovsx"
  url "https://registry.npmjs.org/ovsx/-/ovsx-1.3.0.tgz"
  sha256 "bbdd792bc80942daceb8b79d8c1b35fb392e82aa3a8c7450e46aeafb86c9f45d"
  license "EPL-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "602f18862dfae93182d036fde87ab1240aef93822838773cb693b593fd77842e"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir[libexec/"bin/*"]
  end

  test do
    error = shell_output("#{bin}/ovsx verify-pat 2>&1", 1)
    assert_match "Unable to read the namespace's name", error
  end
end
