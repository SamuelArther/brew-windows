class Autocode < Formula
  desc "Code automation for every language, library and framework"
  homepage "https://autocode.readme.io/"
  url "https://registry.npmjs.org/autocode/-/autocode-1.3.1.tgz"
  sha256 "952364766e645d4ddae30f9d6cc106fdb74d05afc4028066f75eeeb17c4b0247"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bf9b5105a73ca3fe86c11e10d4509b6a567be0cb0fff5673ad3babda4539d43b"
  end

  depends_on "node"

  allow_network_access! :build

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/".autocode/config.yml").write <<~YAML
      name: test
      version: 0.1.0
      description: test description
      author:
        name: Test User
        email: test@example.com
        url: https://example.com
      copyright: 2015 Test
    YAML
    system bin/"autocode", "build"
  end
end
