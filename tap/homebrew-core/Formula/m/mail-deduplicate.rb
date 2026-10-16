class MailDeduplicate < Formula
  desc "CLI to deduplicate mails from mail boxes"
  homepage "https://kdeldycke.github.io/mail-deduplicate/"
  url "https://github.com/kdeldycke/mail-deduplicate/releases/download/v9.3.2/mdedup-9.3.2-windows-x64.exe"
  sha256 "e659a1ce07200df01260e765b45c89c0943d77e1218d30e9ed3d28228df78b5b"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://files.pythonhosted.org/packages/59/4b/0d3d5ee73a939c242551aeb4ef6fab754646f697d481594bd69e0433e819/mail_deduplicate-9.3.2.tar.gz"
  end

  def install
    bin.install "mdedup-9.3.2-windows-x64.exe" => "mdedup.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mdedup --version")

    (testpath/"test_mail1.eml").write <<~EOS
      From: sender1@example.com
      To: recipient@example.com
      Subject: Test Email 1

      This is a test email 1.
    EOS

    (testpath/"test_mail2.eml").write <<~EOS
      From: sender2@example.com
      To: recipient@example.com
      Subject: Test Email 2

      This is a test email 2.
    EOS

    # Run mail-deduplicate to check functionality
    output = shell_output("#{bin}/mdedup --dry-run " \
                          "--export=#{testpath}/deduped_mail.mbox test_mail1.eml test_mail2.eml 2>&1")
    assert_match "No mail selected to perform action on", output
  end
end
