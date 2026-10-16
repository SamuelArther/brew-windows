class VueCli < Formula
  desc "Standard Tooling for Vue.js Development"
  homepage "https://cli.vuejs.org/"
  url "https://registry.npmjs.org/@vue/cli/-/cli-5.0.8.tgz"
  sha256 "29aa4eb0ba827624e42683e1339ebd40e663ad09836dd027df30e3d2108b0b71"
  license "MIT"

  deprecate! date: "2024-12-22", because: :deprecated_upstream
  disable! date: "2025-12-22", because: :deprecated_upstream

  depends_on "node"

  on_macos do
    depends_on "terminal-notifier"
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Remove vendored pre-built binary `terminal-notifier`
    node_notifier_vendor_dir = libexec/"lib/node_modules/@vue/cli/node_modules/node-notifier/vendor"
    rm_r(node_notifier_vendor_dir) # remove vendored pre-built binaries

    if OS.mac?
      terminal_notifier_dir = node_notifier_vendor_dir/"mac.noindex"
      terminal_notifier_dir.mkpath

      # replace vendored `terminal-notifier` with our own
      terminal_notifier_app = formula_opt_prefix("terminal-notifier")/"terminal-notifier.app"
      ln_sf terminal_notifier_app.relative_path_from(terminal_notifier_dir), terminal_notifier_dir
    end
  end

  test do
    (testpath/".vuerc").write <<~JSON
      {
        "useTaobaoRegistry": false,
        "packageManager": "yarn"
      }
    JSON

    assert_match "yarn", shell_output("#{bin}/vue config")
    assert_match "npm", shell_output("#{bin}/vue info")
  end
end
