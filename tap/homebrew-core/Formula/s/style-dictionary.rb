class StyleDictionary < Formula
  desc "Build system for creating cross-platform styles"
  homepage "https://styledictionary.com"
  url "https://registry.npmjs.org/style-dictionary/-/style-dictionary-5.8.0.tgz"
  sha256 "c5e4c89f6ed9ea1cc5058dc1586a107b93cd1a8cc8714542699b71c4e89acc19"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8482dfe41ec9c4ef98ea74b984a8552519f78aff84de0a3b37b65740833f108d"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Build an `:all` bottle by removing example files
    examples = libexec/"lib/node_modules/style-dictionary/examples"
    rm %w[
      advanced/create-react-native-app/android/app/proguard-rules.pro
      complete/android/demo/proguard-rules.pro
    ].map { |file| examples/file }
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/style-dictionary --version")

    output = shell_output("#{bin}/style-dictionary init basic")
    assert_match "Source style dictionary starter files created!", output
    assert_path_exists testpath/"config.json"

    output = shell_output("#{bin}/style-dictionary build")
    assert_match "✔︎ build/css/_variables.css", output
    assert_path_exists testpath/"build/css/_variables.css"
  end
end
