class Nuxi < Formula
  desc "Nuxt CLI (nuxi) for creating and managing Nuxt projects"
  homepage "https://github.com/nuxt/cli"
  url "https://registry.npmjs.org/nuxi/-/nuxi-4.0.1.tgz"
  sha256 "77c39e7f0bfef3be456b1f0c775088770e8ea0140adc514af38766306e29421c"
  license "MIT"
  head "https://github.com/nuxt/cli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a6cfed0aff3b23a784d30d061d9c606e4b4c86a251c0a3515b94ea17e50d986a"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    # Both aliases should be present and report the same version
    assert_match version.to_s, shell_output("#{bin}/nuxi --version")
    assert_match version.to_s, shell_output("#{bin}/nuxt --version")

    # Perform a minimal project initialization in the temporary testpath
    ENV["CI"] = "1"
    target = testpath/"nuxi-tmp"
    output = shell_output(
      "#{bin}/nuxt init . --cwd #{target} -f --template=minimal --gitInit --packageManager=npm --preferOffline",
    )
    assert_predicate target, :directory?
    assert_predicate target/".git", :directory?
    assert_path_exists target/"package.json"
    assert_match "npm run dev", output
  end
end
