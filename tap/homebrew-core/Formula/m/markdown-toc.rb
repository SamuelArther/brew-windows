class MarkdownToc < Formula
  desc "Generate a markdown TOC (table of contents) with Remarkable"
  homepage "https://github.com/jonschlinkert/markdown-toc"
  url "https://registry.npmjs.org/markdown-toc/-/markdown-toc-1.2.0.tgz"
  sha256 "4a5bf3efafb21217889ab240caacd795a1101bfbe07cd8abb228cc44937acd9c"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2757637ae859c8edfcca9c06d14e6d6c0a8aa9dedd9df62bfd2c68269edcd27c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_equal "- [One](#one)\n- [Two](#two)",
      shell_output("bash -c \"#{bin}/markdown-toc - <<< $'# One\\n\\n# Two'\"").strip
  end
end
