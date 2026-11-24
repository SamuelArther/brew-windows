class Quint < Formula
  desc "Core tool for the Quint specification language"
  homepage "https://quint-lang.org"
  url "https://registry.npmjs.org/@informalsystems/quint/-/quint-0.34.0.tgz"
  sha256 "845ecadc2af9a57317e8db84d4d90f8bde8476e90f8248bbf82b0eef100cf5da"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bdf8a079b6b020edeac920f6265a0cd90d85eaf725eb901ac83d6d8a91c46ebf"
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
