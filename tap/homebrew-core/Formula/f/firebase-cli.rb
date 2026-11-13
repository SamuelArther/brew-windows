class FirebaseCli < Formula
  desc "Firebase command-line tools"
  homepage "https://firebase.google.com/docs/cli/"
  url "https://registry.npmjs.org/firebase-tools/-/firebase-tools-15.36.0.tgz"
  sha256 "5784ea62f0b1066a55317d51d6faf4f932fef58c226f7828f5390729c0e56e59"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fa88f0b7822189ba80626db47b5619da9659ea0476d4935f8bed8f2302438628"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    node_modules = libexec/"lib/node_modules/firebase-tools/node_modules"

    # Remove incompatible pre-built `bare-*` binaries
    os = OS.kernel_name.downcase
    arch = Hardware::CPU.intel? ? "x64" : Hardware::CPU.arch.to_s
    node_modules.glob("bare-{buffer,fs,path,os,url}/prebuilds/*")
                .each { |dir| rm_r(dir) if dir.basename.to_s != "#{os}-#{arch}" }
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/firebase --version")

    assert_match "Failed to authenticate", shell_output("#{bin}/firebase projects:list", 1)
  end
end
