using RaceDay.API.Services;
using Xunit;

namespace RaceDay.API.Tests.Services;

public class PasswordHasherTests
{
    // ============================================================
    // TEST 1: Hashing a password produces something different
    // ============================================================
    [Fact]
    public void Hash_ProducesDifferentStringFromOriginal()
    {
        // ARRANGE
        var hasher = new PasswordHasher();
        var originalPassword = "MySecret123";

        // ACT
        var hash = hasher.Hash(originalPassword);

        // ASSERT
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(originalPassword, hash);   // plain text should NOT match hash
    }

    // ============================================================
    // TEST 2: The same password produces a different hash each time
    //         (this is a feature of BCrypt - salting)
    // ============================================================
    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // ARRANGE
        var hasher = new PasswordHasher();

        // ACT
        var hash1 = hasher.Hash("SamePassword");
        var hash2 = hasher.Hash("SamePassword");

        // ASSERT
        Assert.NotEqual(hash1, hash2);   // BCrypt uses random salt each time
    }

    // ============================================================
    // TEST 3: Verify returns TRUE for the correct password
    // ============================================================
    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // ARRANGE
        var hasher = new PasswordHasher();
        var password = "CorrectHorse123";
        var hash = hasher.Hash(password);

        // ACT
        var result = hasher.Verify(password, hash);

        // ASSERT
        Assert.True(result);
    }

    // ============================================================
    // TEST 4: Verify returns FALSE for the wrong password
    // ============================================================
    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        // ARRANGE
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("CorrectPassword");

        // ACT
        var result = hasher.Verify("WrongPassword", hash);

        // ASSERT
        Assert.False(result);
    }

    // ============================================================
    // TEST 5: Verify works with hashes from ANOTHER instance
    //         (proves hashing is deterministic for verification)
    // ============================================================
    [Fact]
    public void Verify_HashFromDifferentInstance_StillWorks()
    {
        // ARRANGE
        var hasherA = new PasswordHasher();
        var hasherB = new PasswordHasher();
        var password = "SharedSecret";

        // ACT
        var hash = hasherA.Hash(password);
        var result = hasherB.Verify(password, hash);

        // ASSERT
        Assert.True(result);
    }
}
