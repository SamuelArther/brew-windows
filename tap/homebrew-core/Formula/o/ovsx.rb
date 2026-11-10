class Ovsx < Formula
  desc "Command-line interface for Eclipse Open VSX"
  homepage "https://www.npmjs.com/package/ovsx"
  url "https://registry.npmjs.org/ovsx/-/ovsx-1.4.0.tgz"
  sha256 "0613791261483819ce3d2944219f8639e91aa4f6b639294d2aa9f8bbced2634e"
  license "EPL-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d45bd0c32e173c26b8795b7c97d23cb0f9e24607e3b98fcc87ea5895f594d506"
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
