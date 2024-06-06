// 1. Usuwanie danych powinno być chronione.
[Authorize]
[HttpPost("delete/{id}")]
// 2. Metoda zwracała void. Zamiast tego, powinna zwracać kod HTTP.
// 3. Bezpieczniej byłoby to robić asynchronicznie.
public async Task<IActionResult> Delete(uint id) {
    // 4. Metoda FirstOrDefaultAsync zwróci null, jeśli użytkownik o podanym id nie istnieje.
    User user = await _context.Users.FirstOrDefaultAsync(user => user.id == id);
    if (user == null) {
        _logger.LogWarning($"User with id={id} not found.");
        return NotFound($"User with id={id} not found.");
    }
    _context.Users.Remove(user);
    await _context.SaveChangesAsync();
    // 5. Debug.WriteLine używany jest do debugowania, ale może lepiej byłoby użyć ILoggera?
    _logger.LogInformation($"The user with Login={user.login} has been deleted.");
    return Ok();
}

// +. Dodatkowo mogą wystąpić z równoczesnością, gdzie dwoje różnych użytkowników
// próbuje zarządzać/usuwać tego samego użytkownika.
// Kod się trochę komplikuje, więc wstawiam tutaj jak można byłoby to rozwiązać.

[Authorize]
[HttpPost("delete/{id}")]
public async Task<IActionResult> Delete(uint id) {
    using (var transaction = await _context.Database.BeginTransactionAsync()) {
        try {
            User user = await _context.Users.FirstOrDefaultAsync(user => user.id == id);
            if (user == null) {
                _logger.LogWarning($"User with id={id} not found.");
                return NotFound($"User with id={id} not found.");
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation($"The user with Login={user.login} has been deleted.");
            return Ok();
        } catch (Exception ex) {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred while deleting the user.");
            return StatusCode(500, "Internal server error.");
        }
    }
}