class Openspec < Formula
  desc "Spec-driven development (SDD) for AI coding assistants"
  homepage "https://openspec.dev/"
  url "https://registry.npmjs.org/@fission-ai/openspec/-/openspec-1.14.2.tgz"
  sha256 "2aed83628ec74f217ee83f1e3bff397f247ec2e8c1f8e93c86d804ba45445c82"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8a3b3a41f1d3130d512cddace6d44ad7f003cf9477f35946012d745e860e3fb2"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
    generate_completions_from_executable(bin/"openspec", "completion", "generate")
  end

  test do
    system bin/"openspec", "init", "--tools", "none"
    assert_path_exists testpath/"openspec/changes"
    assert_path_exists testpath/"openspec/specs"
  end
end
