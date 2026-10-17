class PnpmAT9 < Formula
  desc "Fast, disk space efficient package manager"
  homepage "https://pnpm.io/"
  url "https://registry.npmjs.org/pnpm/-/pnpm-9.15.9.tgz"
  sha256 "cf86a7ad764406395d4286a6d09d730711720acc6d93e9dce9ac7ac4dc4a28a7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4c90d81f373cc71dd7d87c091a2516e3ffe9387ad3757c3c986496a9d4c1dff8"
  end

  keg_only :versioned_formula

  # https://github.com/pnpm/pnpm/commit/7f7c581ce2c8f45c9bcfebfb38c1ce2cebc45ef1
  deprecate! date: "2026-08-06", because: :unsupported
  disable! date: "2027-02-06", because: :unsupported

  depends_on "node" => [:build, :test]

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
    bin.install_symlink bin/"pnpm" => "pnpm@9"
    bin.install_symlink bin/"pnpx" => "pnpx@9"

    generate_completions_from_executable(bin/"pnpm", "completion")

    # remove non-native architecture pre-built binaries
    (libexec/"lib/node_modules/pnpm/dist").glob("reflink.*.node").each do |f|
      next if f.arch == Hardware::CPU.arch

      rm f
    end
  end

  def caveats
    <<~EOS
      pnpm requires a Node installation to function. You can install one with:
        brew install node
    EOS
  end

  test do
    system bin/"pnpm", "init"
    assert_path_exists testpath/"package.json", "package.json must exist"
  end
end
