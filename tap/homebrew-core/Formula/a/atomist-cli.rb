class AtomistCli < Formula
  desc "Unified command-line tool for interacting with Atomist services"
  homepage "https://github.com/atomist/cli"
  url "https://registry.npmjs.org/@atomist/cli/-/cli-1.8.0.tgz"
  sha256 "64bcc7484fa2f1b7172984c278ae928450149fb02b750f79454b1a6683d17f62"
  license "Apache-2.0"
  revision 1

  deprecate! date: "2025-08-01", because: :unmaintained
  disable! date: "2026-08-01", because: :unmaintained

  depends_on "node"

  on_macos do
    depends_on "macos-term-size"
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
    bash_completion.install libexec/"lib/node_modules/@atomist/cli/assets/bash_completion/atomist"

    term_size_vendor_dir = libexec/"lib/node_modules/@atomist/cli/node_modules/term-size/vendor"
    rm_r(term_size_vendor_dir) # remove pre-built binaries

    if OS.mac?
      macos_dir = term_size_vendor_dir/"macos"
      macos_dir.mkpath
      # Replace the vendored pre-built term-size with one we build ourselves
      ln_sf (formula_opt_bin("macos-term-size")/"term-size").relative_path_from(macos_dir), macos_dir
    end
  end

  test do
    assert_path_exists bin/"atomist"
    assert_predicate bin/"atomist", :executable?
    assert_path_exists bin/"@atomist"
    assert_predicate bin/"@atomist", :executable?

    run_output = shell_output("#{bin}/atomist 2>&1", 1)
    assert_match "Not enough non-option arguments", run_output
    assert_match "Specify --help for available options", run_output

    version_output = shell_output("#{bin}/atomist --version")
    assert_match "@atomist/cli", version_output
    assert_match "@atomist/sdm ", version_output
    assert_match "@atomist/sdm-core", version_output
    assert_match "@atomist/sdm-local", version_output

    skill_output = shell_output("#{bin}/atomist show skills")
    assert_match(/\d+ commands are available from \d+ connected SDMs/, skill_output)
  end
end
