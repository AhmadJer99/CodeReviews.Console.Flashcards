using FlashCards.Models;
using FlashCards.Dtos;
using FlashCards.Data;
using FlashCards.Mappers;
using Spectre.Console;

namespace FlashCards.Managers;

internal class CardsManager : ModelManager<Card>
{
    public enum CardOperation
    {
        ShowCardsInStack,
        AddCardToStack,
        SeeAmountOfCardsInStack,
        DeleteCard,
        BackToMenu
    }
    private int _currentStackID;

    private List<CardDto>? _cardsDtos;
    private List<Card>? _cards;
    private readonly CardsDBController _cardsDBController = new();

    public CardsManager()
    {
        SelectStack();
        LoadCardsWithStackId(_currentStackID);
    }

    private void SelectStack()
    {
        List<StackDto> stacksDtos;
        List<Stack> stacks;
        StacksDBController stacksDBController = new();

        StacksManager stacksManager = new StacksManager();
        stacks = stacksDBController.ReadAllRows();

        stacksDtos = stacks.Select(
                s => s.ToStackDto())
                .ToList();
        _currentStackID = stacksManager.ChooseStackMenu();
    }

    private void LoadCardsWithStackId(int _currentStackID)
    {
        _cards = _cardsDBController.ReadAllRows(_currentStackID);

        _cardsDtos = _cards.Select(
                c => c.ToCardDto())
                .ToList();
    }

    public void ShowMenu()
    {
        if (_currentStackID == -1)
            return;

        var userOption = AnsiConsole.Prompt(
            new SelectionPrompt<CardOperation>()
            .Title("[yellow]Choose an operation: [/]")
            .AddChoices(Enum.GetValues<CardOperation>()));

        switch (userOption)
        {
            case CardOperation.ShowCardsInStack:
                ShowCards();
                Console.ReadKey();
                break;
            case CardOperation.AddCardToStack:
                AddNewModel();
                Console.ReadKey();
                break;
            case CardOperation.SeeAmountOfCardsInStack:
                var numOfCardsInStack = _cardsDBController.RowsCount(_currentStackID);
                AnsiConsole.MarkupLine($"[yellow]This stack has {numOfCardsInStack} of _cards in it[/]\n(Press Any Key To Continue)");
                Console.ReadKey();
                break;
            case CardOperation.DeleteCard:
                var cardNumber = ChooseCard();
                if (cardNumber == -1)
                    break;
                DeleteModel(cardNumber);
                Console.ReadKey();
                break;
            case CardOperation.BackToMenu:
                return;
        }
    }

    private void ShowCards()
    {
        var cardsWithSequence = _cardsDBController.ReadAllRows(_currentStackID, true);
        var cardsWithSequenceDtos = cardsWithSequence.Select(
                c => c.ToCardDto())
                .ToList();

        List<string> columnNames = ["Card Number", "Front", "Back"];
        TableVisualisationEngine<CardDto>.ViewAsTable(cardsWithSequenceDtos, ConsoleTableExt.TableAligntment.Left, columnNames);
    }

    private int ChooseCard()
    {
        bool exitMenu = false;
        do
        {
            Console.Clear();
            ShowCards();
            AnsiConsole.MarkupLine("[yellow]Enter a card number (Or enter 'quit' to exit)[/]");

            string? readResult = Console.ReadLine();
            if (string.IsNullOrEmpty(readResult))
            {
                AnsiConsole.MarkupLine("[red]Error- Invalid input[/]");
                continue;
            }

            string userEntry = readResult.Trim();
            exitMenu = userEntry.Equals("quit", StringComparison.CurrentCultureIgnoreCase);
            if (exitMenu)
                continue;

            if (!int.TryParse(userEntry, out int userChoice) || userChoice < 1 || userChoice > _cardsDtos.Count)
            {
                AnsiConsole.MarkupLine("[red]Error- Invalid input, please choose a valid card number.[/]");
                continue;
            }

            var chosenCard = _cards[userChoice - 1]; // User entry is 1-based; list index is 0-based
            AnsiConsole.MarkupLine($"[green]You selected card: {chosenCard.cardnumber}![/]");
            return chosenCard.cardnumber;
        }
        while (!exitMenu);
        return -1;
    }

    protected override void AddNewModel()
    {
        var frontText = AnsiConsole.Ask<string>("[yellow]Enter what you want to be on front of the card\n[/]");
        var backText = AnsiConsole.Ask<string>("[yellow]Enter what you want to be on back of the card\n[/]");

        var card = new Card 
        { 
            FK_stack_id = _currentStackID,
            front = frontText,
            back = backText
        };
        _cardsDBController.InsertRow(card);

        AnsiConsole.MarkupLine("[green]Card Added Succesfully![/]\n(Press Any Key To Continue)");
    }

    protected override void DeleteModel(int cardNumber)
    {
        AnsiConsole.MarkupLine("[red]Are you sure you want to delete this card?\n[white](To Confirm Deletion Press Enter)[/][/]");
        if (Console.ReadKey().Key == ConsoleKey.Enter)
        {
            _cardsDBController.DeleteRow(cardNumber);
            AnsiConsole.MarkupLine("[green]Card Deleted Succesfully![/]");
            return;
        }
        AnsiConsole.MarkupLine("[red]Card Deletion Cancelled! (Press Any Key To Continue)[/]");
    }

    protected override void UpdateModel(int stackId, Card modifiedCard)
    {
        throw new NotImplementedException();
    }
}