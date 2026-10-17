class Quint < Formula
  desc "Core tool for the Quint specification language"
  homepage "https://quint-lang.org"
  url "https://registry.npmjs.org/@informalsystems/quint/-/quint-0.33.0.tgz"
  sha256 "530a8d6bc25533387a5aaab6023de2b4505abe06639d681260eeab6b5aa1b646"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "56a0f8cc10e0a8f601b9f35ac1a1b805be8591d0f3182dbd15a3b637ae43d6ad"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/quint --version")

    (testpath/"bank.qnt").write <<~QNT
      module bank {
        var balances: str -> int
        pure val ADDRESSES = Set("alice", "bob", "charlie")

        action deposit(account, amount) = {
          balances' = balances.setBy(account, curr => curr + amount)
        }

        action withdraw(account, amount) = all {
          balances.get(account) >= amount,
          balances' = balances.setBy(account, curr => curr - amount),
        }

        action init = { balances' = ADDRESSES.mapBy(_ => 0) }

        action step = {
          nondet account = ADDRESSES.oneOf()
          nondet amount = 1.to(100).oneOf()
          any { deposit(account, amount), withdraw(account, amount) }
        }

        val no_negatives = ADDRESSES.forall(addr => balances.get(addr) >= 0)
      }
    QNT

    out = shell_output("#{bin}/quint compile bank.qnt")
    assert_match '"stage":"compiling"', out
    assert_match '"main":"bank"', out
  end
end
