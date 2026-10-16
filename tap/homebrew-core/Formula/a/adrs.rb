class Adrs < Formula
  desc "Architectural Decision Record tool in Rust"
  homepage "https://joshrotenberg.com/adrs/"
  url "https://github.com/joshrotenberg/adrs/releases/download/v0.12.1/adrs-x86_64-pc-windows-msvc.zip"
  sha256 "6fdcd49be36ea91bda232a364b58b016b7f84aa10a80775c2323fcc6e7e0afed"
  license any_of: ["MIT", "Apache-2.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/joshrotenberg/adrs/archive/refs/tags/v0.12.1.tar.gz"
  end

  def install
    bin.install "adrs.exe"
  end

  test do
    # Exercise `adrs doctor` as a CI lint gate: it exits 0 for advisory findings
    # and non-zero for structural errors. Drive a sample ADR through the
    # advisory path — ADR014 (advisory: placeholder text left in a fresh template)
    system bin/"adrs", "init", "docs/decisions"

    # init seeds a Nygard-format ADR; drop it so the demo below is MADR-only.
    (testpath/"docs/decisions/0001-record-architecture-decisions.md").unlink

    system bin/"adrs", "new", "--format", "madr", "--no-edit",
           "Use Homebrew for software installation"
    assert_match "ADR014", shell_output("#{bin}/adrs doctor")

    assert_match version.to_s, shell_output("#{bin}/adrs --version")
  end
end
