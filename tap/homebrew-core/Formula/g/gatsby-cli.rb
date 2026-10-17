class GatsbyCli < Formula
  desc "Gatsby command-line interface"
  homepage "https://www.gatsbyjs.com/docs/reference/gatsby-cli/"
  url "https://registry.npmjs.org/gatsby-cli/-/gatsby-cli-5.16.0.tgz"
  sha256 "312014d94558e08b829c5aadf736ecc6a221d137c3e1bcaa3812bbe23eea5bab"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "43a8526387d90eb58492854f804340596b967c4ebec02d9555c9a772e790449b"
  end

  depends_on "node"

  on_linux do
    depends_on "xsel"
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir[libexec/"bin/*"]

    # Remove incompatible pre-built binaries
    node_modules = libexec/"lib/node_modules/#{name}/node_modules"
    arch = Hardware::CPU.intel? ? "x64" : Hardware::CPU.arch.to_s
    if OS.linux?
      %w[@lmdb/lmdb @msgpackr-extract/msgpackr-extract].each do |mod|
        node_modules.glob("#{mod}-linux-#{arch}/*.musl.node")
                    .map(&:unlink)
                    .empty? && raise("Unable to find #{mod} musl library to delete.")
      end
    end

    clipboardy_fallbacks_dir = node_modules/"clipboardy/fallbacks"
    rm_r(clipboardy_fallbacks_dir) # remove pre-built binaries
    if OS.linux?
      linux_dir = clipboardy_fallbacks_dir/"linux"
      linux_dir.mkpath
      # Replace the vendored pre-built xsel with one we build ourselves
      ln_sf (formula_opt_bin("xsel")/"xsel").relative_path_from(linux_dir), linux_dir
    end
  end

  test do
    # Workaround for https://github.com/nodejs/node-addon-api/issues/1007
    ENV.append "CXXFLAGS", "-std=c++17" if OS.linux?

    system bin/"gatsby", "new", "hello-world", "https://github.com/gatsbyjs/gatsby-starter-hello-world"
    assert_path_exists testpath/"hello-world/package.json", "package.json was not cloned"
  end
end
