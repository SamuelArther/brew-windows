class CubejsCli < Formula
  desc "Cube.js command-line interface"
  homepage "https://cube.dev/"
  url "https://registry.npmjs.org/cubejs-cli/-/cubejs-cli-1.8.8.tgz"
  sha256 "2dd86bb00a7664a2d41f309df3d363b358e9176026f8722cb82870589ad673fd"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2606f29902e37d740b56ea6bb2f22881a7f5dfbf51d2b1641bf81c01350c9223"
  end

  depends_on "node"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def fetch
    system "npm", "install", *std_npm_args(prefix: buildpath/"npm-fetch")
  end

  def install
    rm_r buildpath/"npm-fetch"
    system "npm", "install", "--offline", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    node_modules = libexec/"lib/node_modules/cubejs-cli/node_modules"
    deuniversalize_machos node_modules/"fsevents/fsevents.node" if OS.mac?
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cubejs --version")
    ENV["CI"] = "1" # Disable telemetry.
    token = "eyJhbGciOiJub25lIn0.eyJ1cmwiOiJodHRwczovL2V4YW1wbGUuY29tIn0."
    assert_match "Token successfully added!", shell_output("#{bin}/cubejs auth #{token}")
    config = JSON.parse((testpath/".cubecloud/config.json").read)
    assert_equal token, config.dig("auth", "https://example.com", "auth")
  end
end
